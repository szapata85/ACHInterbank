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
    return this.http.get(this.buildUrl(path), options as never);
  }

  post<T>(path: string, body: unknown, options?: RequestOptions): Observable<T> {
    return this.http.post<T>(this.buildUrl(path), body, options);
  }

  put<T>(path: string, body: unknown, options?: RequestOptions): Observable<T> {
    return this.http.put<T>(this.buildUrl(path), body, options);
  }

  delete<T>(path: string, options?: RequestOptions): Observable<T> {
    return this.http.delete<T>(this.buildUrl(path), options);
  }

  private buildUrl(path: string): string {
    if (/^https?:\/\//i.test(path)) {
      return path;
    }

    const cleanedPath = path.replace(/^\/+/, '');
    return `${this.apiBaseUrl}/${cleanedPath}`;
  }
}
