import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ODataPage, ODataQuery, ODataResponse } from '../models/odata';

/** Reads OData entity sets exposed by the backend under /odata. */
@Injectable({ providedIn: 'root' })
export class ODataService {
  private readonly http = inject(HttpClient);

  query<T>(entitySet: string, query: ODataQuery): Observable<ODataPage<T>> {
    let params = new HttpParams();
    if (query.filter) params = params.set('$filter', query.filter);
    if (query.orderBy) params = params.set('$orderby', query.orderBy);
    if (query.top != null) params = params.set('$top', query.top);
    if (query.skip) params = params.set('$skip', query.skip);
    if (query.count) params = params.set('$count', 'true');
    if (query.select?.length) params = params.set('$select', query.select.join(','));

    return this.http
      .get<ODataResponse<T>>(`${API_BASE_URL}/odata/${entitySet}`, { params })
      .pipe(map((response) => ({ items: response.value, total: response['@odata.count'] ?? response.value.length })));
  }
}

/** Escapes a value for use inside an OData string literal. */
export function odataString(value: string): string {
  return `'${value.replace(/'/g, "''")}'`;
}
