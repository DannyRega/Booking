import { ENV } from '../../config/env';
import { ApiError } from '../../domain/models/Error';

export class HttpClient {
  private static sanitizeUrl(endpoint: string): string {
    return `${ENV.API_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
  }

  public static async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = this.sanitizeUrl(endpoint);
    const headers = new Headers(options.headers || {});

    if (!headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json');
    }

    const token = sessionStorage.getItem('auth_token');
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    try {
      const response = await fetch(url, { ...options, headers });

      if (response.status === 204) {
        return {} as T;
      }

      const data = await response.json().catch(() => ({}));

      if (!response.ok) {
        throw new ApiError(
          response.status,
          data.error || 'Ocurrió un error inesperado al procesar la solicitud.'
        );
      }

      return data as T;
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        throw err;
      }

      throw new ApiError(
        0,
        err instanceof Error ? err.message : 'No fue posible conectar con el servidor. Verifica tu conexión.'
      );
    }
  }
}