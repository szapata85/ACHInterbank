import { IntegrationParameterReadModel } from '../data-access/integration-read.models';

export interface IntegrationTechnicalDetailLine {
  label: string;
  value: string;
}

export function presentParameterTechnicalDetails(parameter: IntegrationParameterReadModel): IntegrationTechnicalDetailLine[] {
  const details = parameter.technicalDetails;
  return [
    { label: 'Nombre técnico', value: parameter.technicalName },
    { label: 'Elemento XML', value: details.xmlElementName },
    { label: 'Tipo', value: details.xsdType || details.dataType },
    { label: 'Dirección', value: details.direction },
    { label: 'Cardinalidad', value: details.cardinality },
    { label: 'Soporte', value: details.supportStatus }
  ];
}
