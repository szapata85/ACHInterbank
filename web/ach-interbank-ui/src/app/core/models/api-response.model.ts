export interface ApiResponse<T> {
  statusCode: number;
  sucess: boolean;
  message?: string | null;
  data: T;
}
