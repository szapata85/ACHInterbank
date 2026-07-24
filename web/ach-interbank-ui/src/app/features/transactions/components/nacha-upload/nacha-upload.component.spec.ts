import { classifyNachaUploadFile } from './nacha-upload.component';

describe('NachaUploadComponent file validation', () => {
  it('should classify ACH Colombia production references without implying homologation', () => {
    const result = classifyNachaUploadFile('0001283.001.20250331.1.OUT');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('production-reference-achcol');
    expect(result.label).toBe('Referencia productiva ACH Colombia');
    expect(result.detail).toContain('no implica homologación normativa');
  });

  it('should classify CENIT production references without adding an extension', () => {
    const result = classifyNachaUploadFile('0001283.002.20250331.1');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('production-reference-cenit');
    expect(result.label).toBe('Referencia productiva CENIT');
    expect(result.detail).toContain('no implica homologación normativa');
  });

  it('should classify ACH Colombia official operational files', () => {
    for (const suffix of ['1', '5', '6', '10']) {
      const result = classifyNachaUploadFile(`0001283.001.${suffix}`);

      expect(result.allowed).toBeTrue();
      expect(result.kind).toBe('official-ach');
      expect(result.label).toBe('Archivo operativo ACH Colombia');
      expect(result.detail).toContain('RRRRTTT.ZZZ.N');
    }
  });

  it('should classify ACH Colombia return files', () => {
    const result = classifyNachaUploadFile('0001283.001.RET');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('official-ret');
    expect(result.label).toBe('Devolución ACH Colombia');
    expect(result.detail).toContain('RRRRTTT.ZZZ.RET');
  });

  it('should classify .ach files as UAT internal fixtures', () => {
    const result = classifyNachaUploadFile('ACH_COL_IN_001.ach');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('uat-fixture');
    expect(result.label).toBe('Fixture UAT/golden interno');
  });

  it('should classify .ach suffix on an official-looking name as a UAT internal fixture, not official', () => {
    const result = classifyNachaUploadFile('0001283.001.1.ach');

    expect(result.allowed).toBeTrue();
    expect(result.kind).toBe('uat-fixture');
    expect(result.label).toBe('Fixture UAT/golden interno');
  });

  it('should reject txt, nacha and env files', () => {
    for (const fileName of ['rechazo.txt', 'rechazo.nacha', 'rechazo.env']) {
      const result = classifyNachaUploadFile(fileName);

      expect(result.allowed).toBeFalse();
      expect(result.kind).toBe('rejected');
      expect(result.rejectionMessage).toContain('no se admiten');
    }
  });

  it('should reject unknown extensions', () => {
    const result = classifyNachaUploadFile('archivo.bin');

    expect(result.allowed).toBeFalse();
    expect(result.kind).toBe('rejected');
    expect(result.rejectionMessage).toContain('nombre operativo ACHCOL/CENIT');
  });
});
