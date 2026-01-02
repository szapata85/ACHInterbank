export interface LoginRequestModel {
  username: string;
  password: string;
}

export interface AuthPayload {
  token: string;
  expiresAt?: string;
  username?: string;
  fullName?: string;
  roles?: string[];
  permissions?: string[];
}

export interface UserSession {
  token: string;
  username: string;
  fullName: string;
  roles: string[];
  permissions: string[];
  issuedAt?: Date;
  expiresAt?: Date;
  userId?: string;
}
