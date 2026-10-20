import { expect, type Locator } from '@playwright/test';

const forbiddenPresentationTerms: ReadonlyArray<{ term: string; source: string }> = [
  { term: 'Legacy', source: '\\bLegacy\\b' },
  { term: 'Layout', source: '\\bLayout\\b' },
  { term: 'BatchCount', source: '\\bBatchCount\\b' },
  { term: 'Profile', source: '\\bProfile\\b' },
  { term: 'Record', source: '\\bRecord\\b' },
  { term: 'Variant', source: '\\bVariant\\b' },
  { term: 'Field', source: '\\bField\\b' },
  { term: 'Master', source: '\\bMaster\\b' },
  { term: 'Detail', source: '\\bDetail\\b' },
  { term: 'Source', source: '\\bSource\\b' },
  { term: 'Property', source: '\\bProperty\\b' },
  { term: 'Path', source: '\\bPath\\b' },
  { term: 'Default', source: '\\bDefault\\b' },
  { term: 'Enabled', source: '\\bEnabled\\b' },
  { term: 'Disabled', source: '\\bDisabled\\b' },
  { term: 'Required', source: '\\bRequired\\b' },
  { term: 'Warning', source: '\\bWarning\\b' },
  { term: 'Left', source: '\\bLeft\\b' },
  { term: 'Right', source: '\\bRight\\b' },
  { term: 'Padding', source: '\\bPadding\\b' },
  { term: 'Format', source: '\\bFormat\\b' },
  { term: 'Mask', source: '\\bMask\\b' },
  { term: 'Fallback', source: '\\bFallback\\b' },
  { term: 'Pipeline', source: '\\bPipeline\\b' },
  { term: 'Expression', source: '\\bExpression\\b' },
  { term: 'External catalog', source: '\\bExternal\\s+catalog\\b' },
  { term: 'Entity', source: '\\bEntity\\b' },
  { term: 'Constant', source: '\\bConstant\\b' },
  { term: 'Table driven', source: '\\bTable\\s+driven\\b' },
  { term: 'Draft', source: '\\bDraft\\b' },
  { term: 'Published', source: '\\bPublished\\b' },
  { term: 'Read only', source: '\\bRead\\s+only\\b' },
  { term: 'Backoffice', source: '\\bBackoffice\\b' }
];

export async function assertNoFunctionalSpanglish(root: Locator): Promise<void> {
  const violations = await root.evaluate((container, forbidden) => {
    const patterns = forbidden.map(item => ({
      term: item.term,
      pattern: new RegExp(item.source, 'i')
    }));
    const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT);
    const found: Array<{ term: string; text: string }> = [];
    let node = walker.nextNode();
    while (node) {
      const parent = node.parentElement;
      const text = node.textContent?.replace(/\s+/g, ' ').trim() ?? '';
      if (parent && text && !parent.closest('[data-technical-value="true"]')) {
        const style = getComputedStyle(parent);
        if (style.display !== 'none' && style.visibility !== 'hidden') {
          for (const item of patterns) {
            if (item.pattern.test(text)) {
              found.push({ term: item.term, text });
            }
          }
        }
      }
      node = walker.nextNode();
    }
    return found;
  }, forbiddenPresentationTerms);

  expect(violations, 'No debe existir inglés funcional fuera de nodos técnicos explícitos.').toEqual([]);
}

export async function collectVisibleFunctionalText(root: Locator): Promise<string[]> {
  return root.evaluate((container) => {
    const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT);
    const texts: string[] = [];
    let node = walker.nextNode();
    while (node) {
      const parent = node.parentElement;
      const text = node.textContent?.replace(/\s+/g, ' ').trim() ?? '';
      if (parent && text && !parent.closest('[data-technical-value="true"]')) {
        const style = getComputedStyle(parent);
        if (style.display !== 'none' && style.visibility !== 'hidden') {
          texts.push(text);
        }
      }
      node = walker.nextNode();
    }
    return [...new Set(texts)];
  });
}
