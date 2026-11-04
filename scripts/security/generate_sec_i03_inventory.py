#!/usr/bin/env python3
"""SEC-I03: inventario/SBOM fuente reproducible y estrictamente offline.

No es un escáner CVE. npm proviene del lockfile (no instalaciones); NuGet
representa SOLO PackageReference declarados (no grafo restaurado), imágenes
representan tags FROM sin digest ni componentes de SO.
"""
from __future__ import annotations
import argparse
import base64
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path
from urllib.parse import quote

SCHEMA='http://cyclonedx.org/schema/bom-1.6.schema.json'
GENERATOR='ACHInterbank SEC-I03 offline source inventory'

def dump(path: Path, data: object):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False, sort_keys=True)+'\n', encoding='utf-8')

def hash_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def prop(key: str, value: object) -> dict:
    return {'name': f'ach:sec_i03:{key}', 'value': str(value).lower() if isinstance(value, bool) else str(value)}

def npm_name(path: str) -> str:
    return path.rsplit('node_modules/', 1)[-1]

def resolve_dep(pkg_path: str, dep_name: str, paths: set[str]) -> str | None:
    # Node resolution: local node_modules then ascend through ancestor packages.
    base=pkg_path
    while base:
        candidate=f'{base}/node_modules/{dep_name}'
        if candidate in paths:
            return candidate
        marker=base.rfind('/node_modules/')
        if marker>=0:
            base=base[:marker]
        else:
            base=''
    candidate=f'node_modules/{dep_name}'
    return candidate if candidate in paths else None

def bom(name: str, properties: list[dict], components: list[dict], dependencies: list[dict]) -> dict:
    return {'$schema': SCHEMA, 'bomFormat':'CycloneDX', 'specVersion':'1.6',
            'version':1, 'metadata':{'component':{'type':'application','name':name,'bom-ref':f'app:{name.lower().replace(" ","-")}'},'properties':properties},
            'components':components, 'dependencies':dependencies, 'compositions':[{'aggregate':'incomplete'}]}

def npm_inventory(root: Path) -> tuple[dict,dict]:
    package_file=root/'web/ach-interbank-ui/package.json'
    lock_file=root/'web/ach-interbank-ui/package-lock.json'
    package=json.loads(package_file.read_text(encoding='utf-8'))
    lock=json.loads(lock_file.read_text(encoding='utf-8'))
    if lock.get('lockfileVersion')!=3: raise ValueError('SEC-I03 requires npm lockfileVersion=3')
    pkgs=lock.get('packages')
    if not isinstance(pkgs,dict) or '' not in pkgs: raise ValueError('missing npm packages root')
    root_deps=pkgs['']
    if any(package.get(k,{}) != root_deps.get(k,{}) for k in ('dependencies','devDependencies')):
        raise ValueError('package.json and package-lock.json direct deps do not match')
    entries=[];components=[]; deps_graph=[]; paths=set(pkgs)-{''}; missing_integrity=[]
    for path,rec in sorted(pkgs.items()):
        if not path:continue
        name=npm_name(path);version=rec.get('version');integrity=rec.get('integrity')
        if not version:raise ValueError('unversioned npm package '+path)
        bref='npm:'+path
        purl='pkg:npm/'+quote(name,safe='/')+'@'+quote(version,safe='')
        comp={'type':'library','bom-ref':bref,'name':name,'version':version,'purl':purl,
              'properties':[prop('npm_lock_path',path),prop('source_type','lockfile_unverified')]}
        if integrity and integrity.startswith('sha512-'):
            try: comp['hashes']=[{'alg':'SHA-512','content':base64.b64decode(integrity.split('-',1)[1],validate=True).hex()}]
            except Exception: raise ValueError('invalid integrity base64 '+path)
        if not integrity:missing_integrity.append(path)
        if rec.get('resolved'):
            comp['externalReferences']=[{'type':'distribution','url':rec['resolved']}]
        components.append(comp)
        requires=set(rec.get('dependencies',{})) | set(rec.get('optionalDependencies',{}))
        resolved={resolve_dep(path,n,paths) for n in requires}
        deps_graph.append({'ref':bref,'dependsOn':sorted('npm:'+x for x in resolved if x)})
        entries.append({'path':path,'name':name,'version':version,'resolved':rec.get('resolved'),
                        'has_integrity':bool(integrity),'dev':bool(rec.get('dev',False)),
                        'optional':bool(rec.get('optional',False))})
    direct=list(dict.fromkeys(list(package.get('dependencies',{}))+list(package.get('devDependencies',{}))))
    rootrefs={resolve_dep('',n,paths) for n in direct}
    unresolved=sorted(n for n in direct if resolve_dep('',n,paths) is None)
    the_bom=bom('ACHInterbank SPA npm lockfile',
                [prop('completeness','npm_lock_resolution_only'),prop('network_validation','NOT_RUN_ENVIRONMENT'),prop('npm_lock_sha256',hash_file(lock_file))],
                components, [{'ref':'app:achinterbank-spa-npm-lockfile','dependsOn':sorted('npm:'+n for n in rootrefs if n)}]+deps_graph)
    inventory={'source':'web/ach-interbank-ui/package-lock.json','source_sha256':hash_file(lock_file),
               'lockfile_version':3,'total_packages':len(entries),'direct_dependencies':len(package.get('dependencies',{})),
               'direct_dev_dependencies':len(package.get('devDependencies',{})),'packages_missing_integrity':len(missing_integrity),
               'missing_integrity_paths':missing_integrity,'unresolved_direct_dependencies':unresolved,
               'npm_overrides':package.get('overrides',{}),'packages':entries,
               'coverage':'lockfile only; no npm install, registry verification, CVE or license audit'}
    return inventory,the_bom

def nuget_inventory(root: Path) -> tuple[dict,dict]:
    refs=[];projects=[];comps={}
    for path in sorted(root.rglob('*.csproj')):
        if any(d in ('obj','bin','.git','node_modules') for d in path.parts):continue
        rel=path.relative_to(root).as_posix()
        try:tree=ET.parse(path)
        except ET.ParseError as ex:raise ValueError(f'Invalid csproj: {rel}: {ex}')
        project_refs=0
        for pkg in tree.getroot().iter('PackageReference'):
            name=pkg.get('Include') or pkg.get('Update')
            if not name:continue
            version=pkg.get('Version') or pkg.findtext('Version') or pkg.get('VersionOverride') or pkg.findtext('VersionOverride')
            entry={'project':rel,'name':name,'declared_version':version,'is_central_or_unresolved':not bool(version) or '$(' in str(version),
                   'condition':pkg.get('Condition')}
            refs.append(entry)
            if version and '$(' not in version:
                ident=(name.lower(),version)
                comps.setdefault(ident,{'type':'library','bom-ref':f'nuget:{name.lower()}@{version}',
                                        'name':name,'version':version,'purl':f'pkg:nuget/{quote(name,safe="")}@{quote(version,safe="")}',
                                        'properties':[prop('nuget_scope','DIRECT_DECLARATION_UNRESTORED')]})
            project_refs+=1
        projects.append({'path':rel,'package_references':project_refs,'sha256':hash_file(path)})
    assets=[p.relative_to(root).as_posix() for p in root.rglob('project.assets.json') if 'obj' in p.parts]
    # Los bin/obj del ZIP pueden ser obsoletos tras actualizar PackageReference.
    components=sorted(comps.values(),key=lambda c:c['bom-ref'])
    nuget_bom=bom('ACHInterbank NuGet declared references',
                  [prop('completeness','direct_package_references_only_transitives_unknown'),prop('restore','NOT_RUN_ENVIRONMENT'),
                   prop('project_assets','PRESENT_UNTRUSTED_LEGACY_NOT_USED')], components,
                  [{'ref':'app:achinterbank-nuget-declared-references', 'dependsOn':[c['bom-ref'] for c in components]}])
    return {'source':'*.csproj','project_count':len(projects),'projects':projects,'package_reference_count':len(refs),
            'unique_declared_packages':len(comps),'references':refs,'packages_lock_files':[],
            'legacy_project_assets_excluded':sorted(assets),
            'coverage':'PackageReference ONLY; NO NuGet restore, transitive dependency/SCA results, license audit or resolved SBOM'},nuget_bom

def image_inventory(root:Path)->dict:
    dockerfiles=[];images=[]
    for path in sorted(root.rglob('Dockerfile')):
        if any(d in ('bin','obj','.git','node_modules') for d in path.parts):continue
        rel=path.relative_to(root).as_posix(); lines=path.read_text(encoding='utf-8-sig').splitlines()
        dockerfiles.append(rel);stages=set()
        for lineno,line in enumerate(lines,1):
            match=re.match(r'^\s*FROM\s+(?:--platform=\S+\s+)?(\S+)(?:\s+AS\s+(\S+))?',line,re.I)
            if not match:continue
            name=match.group(1);stage=match.group(2)
            internal=name.lower() in stages
            if not internal:
                images.append({'dockerfile':rel,'line':lineno,'image':name,'is_pinned_by_digest':'@sha256:' in name,
                               'runtime_actual_digest':'NOT_VERIFIED','os_packages':'UNKNOWN_UNTIL_IMAGE_SCAN'})
            if stage:stages.add(stage.lower())
    return {'dockerfiles':dockerfiles,'declared_images':images,'total_declared_external_images':len(images),
            'unpinned_image_count':sum(not x['is_pinned_by_digest'] for x in images),
            'coverage':'Docker FROM only; no pull, digests, container layers, Alpine/Debian packages or CVE scan'}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--root',required=True,type=Path)
    ap.add_argument('--out',required=True,type=Path)
    args=ap.parse_args(); root=args.root.resolve();out=args.out.resolve()
    np,nb=npm_inventory(root);nu,ub=nuget_inventory(root);img=image_inventory(root)
    dump(out/'SEC-I03_INVENTARIO_FUENTE.json',{'schema':'sec-i03-source-inventory-v1','status':'SOURCE_ONLY_UNVERIFIED_RUNTIME',
         'npm':np,'nuget':nu,'docker':img,'vulnerability_scan':'NOT_RUN_ENVIRONMENT',
         'license_review':'NOT_RUN_ENVIRONMENT','os_sbom':'NOT_RUN_ENVIRONMENT'})
    dump(out/'SEC-I03_NPM_LOCK.cdx.json',nb)
    dump(out/'SEC-I03_NUGET_DECLARADO.cdx.json',ub)
    summary={'status':'PASS_STATIC','npm_packages':np['total_packages'],'npm_missing_integrity':np['packages_missing_integrity'],
             'npm_direct_unresolved':np['unresolved_direct_dependencies'],
             'nuget_projects':nu['project_count'],'nuget_direct_references':nu['package_reference_count'],
             'nuget_unique_declared_packages':nu['unique_declared_packages'],
             'docker_declared_images':img['total_declared_external_images'],'docker_images_unpinned':img['unpinned_image_count'],
             'nuget_transitive_graph':'NOT_RUN_ENVIRONMENT','npm_remote_audit':'NOT_RUN_ENVIRONMENT',
             'docker_layer_scan':'NOT_RUN_ENVIRONMENT', 'artifacts':{}}
    for k in ['SEC-I03_INVENTARIO_FUENTE.json','SEC-I03_NPM_LOCK.cdx.json','SEC-I03_NUGET_DECLARADO.cdx.json']:
        summary['artifacts'][k]=hash_file(out/k)
    dump(out/'SEC-I03_VALIDACION_ESTATICA.json',summary)
    print(json.dumps({k:v for k,v in summary.items() if k!='artifacts'},indent=2,ensure_ascii=False))
    return 0
if __name__=='__main__':sys.exit(main())
