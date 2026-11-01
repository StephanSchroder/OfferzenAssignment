export type ColumnType = 'text' | 'number' | 'currency' | 'date' | 'enum' | 'boolean';

export interface ColumnDef<T> {
  /** Property name, as returned by the OData endpoint (camelCase). */
  field: keyof T & string;
  header: string;
  type?: ColumnType;
  /** Defaults to true. */
  sortable?: boolean;
  /** Defaults to true. */
  filterable?: boolean;
  /** Values for 'enum' columns. */
  options?: readonly string[];
  /** Right-align numbers etc. Defaults based on type. */
  align?: 'start' | 'end';
  width?: string;
}

export type SortDirection = 'asc' | 'desc';

export interface SortState {
  field: string;
  direction: SortDirection;
}

export type ComparisonOperator = 'eq' | 'gt' | 'ge' | 'lt' | 'le';

export interface ColumnFilter {
  value: string;
  operator: ComparisonOperator;
}
