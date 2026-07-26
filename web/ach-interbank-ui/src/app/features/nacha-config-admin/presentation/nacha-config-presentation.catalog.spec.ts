import {
  directionPresentation,
  fieldPresentation,
  flowPresentation,
  justificationPresentation,
  profilePresentation,
  servicePresentation,
  severityPresentation,
  sourceStrategyPresentation,
  sourceTypePresentation,
  statusPresentation,
  variantPresentation
} from './nacha-config-presentation.catalog';

describe('NachaConfigPresentationCatalog', () => {
  it('should present legacy profile, variant and field names in Spanish without changing their codes', () => {
    expect(profilePresentation('LEGACY_ACH_SALIDA_ORIGINAL_V1_0', 'Perfil legado ACH salida original')).toEqual({
      functionalName: 'Perfil heredado de salidas ACH',
      technicalValue: 'LEGACY_ACH_SALIDA_ORIGINAL_V1_0'
    });
    expect(variantPresentation('LEGACY_R9_BASE', 'Layout legado registro 9', '9')).toEqual({
      functionalName: 'Variante base del control de archivo',
      technicalValue: 'LEGACY_R9_BASE'
    });
    expect(fieldPresentation('R9_BATCHCOUNT', 'BatchCount')).toEqual({
      functionalName: 'Cantidad de lotes',
      technicalValue: 'R9_BATCHCOUNT'
    });
  });

  it('should cover every legacy field suffix used by the controlled profile', () => {
    expect(fieldPresentation('R1_IMMEDIATEDESTINATION', 'ImmediateDestination').functionalName).toBe('Destino inmediato');
    expect(fieldPresentation('R5_STANDARDENTRYCLASSCODE', 'StandardEntryClassCode').functionalName).toBe('Código de clase de entrada estándar');
    expect(fieldPresentation('R6_ACCOUNTNUMBER', 'AccountNumber').functionalName).toBe('Número de cuenta');
    expect(fieldPresentation('R7_ENTRYDETAILSEQUENCENUMBER', 'EntryDetailSequenceNumber').functionalName).toBe('Secuencia del detalle de la entrada');
    expect(fieldPresentation('R8_MESSAGEAUTHENTICATIONCODE', 'MessageAuthenticationCode').functionalName).toBe('Código de autenticación del mensaje');
    expect(fieldPresentation('R9_TOTALCREDITAMOUNT', 'TotalCreditAmount').functionalName).toBe('Valor total de créditos');
  });

  it('should preserve an existing Spanish functional name before using a derived fallback', () => {
    expect(fieldPresentation('CUSTOM_FIELD', 'Referencia operativa').functionalName).toBe('Referencia operativa');
    expect(variantPresentation('CUSTOM_VARIANT', 'Variante de contingencia').functionalName).toBe('Variante de contingencia');
  });

  it('should translate persisted enumerations only for presentation', () => {
    expect(statusPresentation('PUBLICADO')).toBe('Publicado');
    expect(statusPresentation('BORRADOR')).toBe('Borrador');
    expect(sourceTypePresentation('ENTIDAD')).toBe('Entidad del dominio');
    expect(sourceTypePresentation('CONSTANTE')).toBe('Valor constante');
    expect(sourceTypePresentation('EXPRESION')).toBe('Expresión calculada');
    expect(sourceTypePresentation('CATALOGO_EXTERNO')).toBe('Catálogo externo');
    expect(sourceStrategyPresentation('TABLE_DRIVEN')).toBe('Configuración parametrizada');
    expect(flowPresentation('ORIGINAL')).toBe('Operaciones originales');
    expect(directionPresentation('SALIDA')).toBe('Salida');
    expect(servicePresentation('PPD')).toBe('Pagos y depósitos preacordados');
    expect(severityPresentation('WARN')).toBe('Advertencia');
    expect(severityPresentation('ERROR')).toBe('Error bloqueante');
    expect(justificationPresentation('R')).toBe('Alineación a la derecha');
    expect(justificationPresentation('L')).toBe('Alineación a la izquierda');
  });
});
