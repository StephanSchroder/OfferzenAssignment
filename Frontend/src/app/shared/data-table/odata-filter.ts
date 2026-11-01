import { odataString } from '../../core/services/odata.service';
import { ColumnDef, ColumnFilter } from './data-table.models';

/** Builds an OData $filter expression for one column, or null when the filter is empty / invalid. */
export function buildColumnFilter<T>(column: ColumnDef<T>, filter: ColumnFilter | undefined): string | null {
  const raw = filter?.value.trim();
  if (!raw) {
    return null;
  }

  const field = column.field;
  switch (column.type ?? 'text') {
    case 'text':
      return `contains(tolower(${field}),${odataString(raw.toLowerCase())})`;
    case 'enum':
      return `${field} eq ${odataString(raw)}`;
    case 'boolean':
      return raw === 'true' || raw === 'false' ? `${field} eq ${raw}` : null;
    case 'number':
    case 'currency': {
      const value = Number(raw.replace(',', '.'));
      return Number.isFinite(value) ? `${field} ${filter!.operator} ${value}` : null;
    }
    case 'date': {
      // Input is a yyyy-MM-dd date in the user's time zone; compare against that day's bounds in UTC.
      const start = new Date(`${raw}T00:00:00`);
      if (Number.isNaN(start.getTime())) {
        return null;
      }
      const end = new Date(start);
      end.setDate(end.getDate() + 1);
      const from = start.toISOString();
      const to = end.toISOString();
      switch (filter!.operator) {
        case 'eq':
          return `${field} ge ${from} and ${field} lt ${to}`; // on that day
        case 'gt':
          return `${field} ge ${to}`; // after that day
        case 'ge':
          return `${field} ge ${from}`; // on or after
        case 'lt':
          return `${field} lt ${from}`; // before that day
        case 'le':
          return `${field} lt ${to}`; // on or before
      }
    }
  }
  return null;
}

export function combineFilters(parts: (string | null | undefined)[]): string | undefined {
  const active = parts.filter((p): p is string => !!p);
  if (active.length === 0) return undefined;
  if (active.length === 1) return active[0];
  return active.map((p) => `(${p})`).join(' and ');
}
