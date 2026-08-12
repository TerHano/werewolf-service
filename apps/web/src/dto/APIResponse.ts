export interface APIResponse<T = void> {
  success: boolean;
  errorMessages?: string[];
  data?: T;
}
