export interface ODataResponse<T> {
  '@odata.count'?: number;
  '@odata.nextLink'?: string;
  value: T[];
}

export interface ODataQuery {
  filter?: string;
  orderBy?: string;
  top?: number;
  skip?: number;
  count?: boolean;
  select?: string[];
}

export interface ODataPage<T> {
  items: T[];
  total: number;
}
