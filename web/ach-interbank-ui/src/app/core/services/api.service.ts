import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type JsonRequestOptions = {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  params?: HttpParams | { [param: string]: string | number | boolean | readonly (string | number | boolean)[] };
  reportProgress?: boolean;
  responseType?: 'json';
  observe?: 'body';
  withCredentials?: boolean;
};

export type BlobBodyRequestOptions = {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  params?: HttpParams | { [param: string]: string | number | boolean | readonly (string | number | boolean)[] };
  reportProgress?: boolean;
  responseType: 'blob';
  observe?: 'body';
  withCredentials?: boolean;
};

export type BlobResponseRequestOptions = {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  params?: HttpParams | { [param: string]: string | number | boolean | readonly (string | number | boolean)[] };
  reportProgress?: boolean;
  responseType: 'blob';
  observe: 'response';
  withCredentials?: boolean;
};

export type RequestOptions = JsonRequestOptions | BlobBodyRequestOptions | BlobResponseRequestOptions;

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http: HttpClient = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  get<T>(path: string, options?: JsonRequestOptions): Observable<T>;
  get(path: string, options: BlobBodyRequestOptions): Observable<Blob>;
  get(path: string, options: BlobResponseRequestOptions): Observable<HttpResponse<Blob>>;
  get<T>(path: string, options?: RequestOptions): Observable<T | Blob | HttpResponse<Blob>> {
    const url = this.buildUrl(path);

    if (options?.responseType === 'blob') {
      if (options.observe === 'response') {
        return this.http.get<Blob>(url, options);
      }

      return this.http.get<Blob>(url, options);
    }

    return this.http.get<T>(url, options);
  }

  post<T>(path: string, body: unknown, options?: JsonRequestOptions): Observable<T>;
  post(path: string, body: unknown, options: BlobBodyRequestOptions): Observable<Blob>;
  post(path: string, body: unknown, options: BlobResponseRequestOptions): Observable<HttpResponse<Blob>>;
  post<T>(path: string, body: unknown, options?: RequestOptions): Observable<T | Blob | HttpResponse<Blob>> {
    const url = this.buildUrl(path);

    if (options?.responseType === 'blob') {
      if (options.observe === 'response') {
        return this.http.post<Blob>(url, body, options);
      }

      return this.http.post<Blob>(url, body, options);
    }

    return this.http.post<T>(url, body, options);
  }

  put<T>(path: string, body: unknown, options?: JsonRequestOptions): Observable<T>;
  put(path: string, body: unknown, options: BlobBodyRequestOptions): Observable<Blob>;
  put(path: string, body: unknown, options: BlobResponseRequestOptions): Observable<HttpResponse<Blob>>;
  put<T>(path: string, body: unknown, options?: RequestOptions): Observable<T | Blob | HttpResponse<Blob>> {
    const url = this.buildUrl(path);

    if (options?.responseType === 'blob') {
      if (options.observe === 'response') {
        return this.http.put<Blob>(url, body, options);
      }

      return this.http.put<Blob>(url, body, options);
    }

    return this.http.put<T>(url, body, options);
  }

  delete<T>(path: string, options?: JsonRequestOptions): Observable<T>;
  delete(path: string, options: BlobBodyRequestOptions): Observable<Blob>;
  delete(path: string, options: BlobResponseRequestOptions): Observable<HttpResponse<Blob>>;
  delete<T>(path: string, options?: RequestOptions): Observable<T | Blob | HttpResponse<Blob>> {
    const url = this.buildUrl(path);

    if (options?.responseType === 'blob') {
      if (options.observe === 'response') {
        return this.http.delete<Blob>(url, options);
      }

      return this.http.delete<Blob>(url, options);
    }

    return this.http.delete<T>(url, options);
  }

  private buildUrl(path: string): string {
    if (/^https?:\/\//i.test(path)) {
      return path;
    }

    const cleanedPath = path.replace(/^\/+/, '');
    return `${this.apiBaseUrl}/${cleanedPath}`;
  }
}
