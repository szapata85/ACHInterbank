export type CapabilityState = 'Enabled' | 'Disabled' | 'ShadowOnly' | 'NotSupported' | 'Planned';
export type CapabilitySource = 'RegistryOverride' | 'StrategyDefault';

export interface PaymentRailItem {
  railCode: string;
  displayName: string;
  isKnownRail: boolean;
  isOperational: boolean;
  source: string;
  version: string;
}

export interface PaymentRailCapabilityItem {
  railCode: string;
  capabilityCode: string;
  state: CapabilityState;
  source: CapabilitySource;
  notes?: string | null;
  evaluatedAtUtc: string;
  effectiveFromUtc?: string | null;
  effectiveToUtc?: string | null;
  version: string;
  changeSource?: string | null;
  changeTicket?: string | null;
  changedBy?: string | null;
  changedAtUtc?: string | null;
}
