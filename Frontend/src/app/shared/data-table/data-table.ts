import { CurrencyPipe, DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, DestroyRef, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, of, switchMap, tap } from 'rxjs';
import { CURRENCY_CODE } from '../../core/api.config';
import { describeHttpError } from '../../core/http-error';
import { ODataPage, ODataQuery } from '../../core/models/odata';
import { ODataService } from '../../core/services/odata.service';
import { Modal } from '../modal/modal';
import { ColumnDef, ColumnFilter, ComparisonOperator, SortState } from './data-table.models';
import { buildColumnFilter, combineFilters } from './odata-filter';

interface OperatorOption {
  value: ComparisonOperator;
  label: string;
}

const NUMBER_OPERATORS: OperatorOption[] = [
  { value: 'eq', label: '=' },
  { value: 'gt', label: '>' },
  { value: 'ge', label: '≥' },
  { value: 'lt', label: '<' },
  { value: 'le', label: '≤' },
];

const DATE_OPERATORS: OperatorOption[] = [
  { value: 'eq', label: 'On' },
  { value: 'ge', label: 'From' },
  { value: 'le', label: 'Until' },
];

/**
 * Reusable table backed by an OData entity set. Handles server-side filtering ($filter),
 * sorting ($orderby) and paging ($top / $skip / $count). The header has Add and Update
 * buttons; each row has a delete (or archive) button with a confirmation step.
 */
@Component({
  selector: 'app-data-table',
  imports: [CurrencyPipe, DatePipe, NgTemplateOutlet, Modal],
  templateUrl: './data-table.html',
  styleUrl: './data-table.css',
})
export class DataTable<T extends { id: number }> {
  private readonly odata = inject(ODataService);

  /** OData entity set name, e.g. "Customers". */
  readonly entitySet = input.required<string>();
  readonly columns = input.required<ColumnDef<T>[]>();
  readonly heading = input.required<string>();
  /** Singular noun used on buttons, e.g. "customer". */
  readonly itemName = input.required<string>();
  /** Extra filter that is always applied, e.g. "isArchived eq false". */
  readonly baseFilter = input<string | null>(null);
  readonly defaultSort = input<SortState | null>(null);
  readonly deleteLabel = input('Delete');
  readonly deleteConfirmation = input<(row: T) => string>(() => 'This item will be removed from the list.');
  readonly canDelete = input<(row: T) => boolean>(() => true);
  /** When true (default) clicking a row selects it; when false it only emits rowClick. */
  readonly rowClickSelects = input(true);

  readonly add = output<void>();
  readonly update = output<T>();
  readonly deleteRow = output<T>();
  readonly rowClick = output<T>();

  protected readonly currencyCode = CURRENCY_CODE;
  protected readonly numberOperators = NUMBER_OPERATORS;
  protected readonly dateOperators = DATE_OPERATORS;
  protected readonly pageSizes = [10, 25, 50];

  protected readonly rows = signal<T[]>([]);
  protected readonly total = signal(0);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly selected = signal<T | null>(null);
  protected readonly pendingDelete = signal<T | null>(null);

  protected readonly sort = signal<SortState | null>(null);
  protected readonly filters = signal<Record<string, ColumnFilter>>({});
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(10);
  private readonly reloadCounter = signal(0);

  protected readonly hasFilters = computed(() => Object.values(this.filters()).some((f) => f.value.trim() !== ''));
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  protected readonly rangeStart = computed(() => (this.total() === 0 ? 0 : this.pageIndex() * this.pageSize() + 1));
  protected readonly rangeEnd = computed(() => Math.min(this.total(), (this.pageIndex() + 1) * this.pageSize()));

  private readonly query = computed<ODataQuery & { entitySet: string; reload: number }>(() => {
    const columns = this.columns();
    const filters = this.filters();
    const sort = this.currentSort();
    return {
      entitySet: this.entitySet(),
      filter: combineFilters([this.baseFilter(), ...columns.map((c) => buildColumnFilter(c, filters[c.field]))]),
      orderBy: sort ? `${sort.field} ${sort.direction}` : 'id desc',
      top: this.pageSize(),
      skip: this.pageIndex() * this.pageSize(),
      count: true,
      reload: this.reloadCounter(),
    };
  });

  constructor() {
    toObservable(this.query)
      .pipe(
        debounceTime(200),
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap(({ entitySet, reload: _reload, ...query }) =>
          this.odata.query<T>(entitySet, query).pipe(
            catchError((error: unknown) => {
              this.error.set(describeHttpError(error));
              return of<ODataPage<T> | null>(null);
            }),
          ),
        ),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((page) => {
        this.loading.set(false);
        if (!page) {
          return;
        }

        // Stepped past the last page (e.g. after deleting its only row): go back one page.
        if (page.items.length === 0 && page.total > 0 && this.pageIndex() > 0) {
          this.pageIndex.set(Math.max(0, Math.ceil(page.total / this.pageSize()) - 1));
          return;
        }

        this.rows.set(page.items);
        this.total.set(page.total);
        const selectedId = this.selected()?.id;
        this.selected.set(page.items.find((row) => row.id === selectedId) ?? null);
      });
  }

  /** Re-runs the current query, e.g. after a row was added, updated or deleted. */
  reload(): void {
    this.reloadCounter.update((n) => n + 1);
  }

  protected value(row: T, column: ColumnDef<T>): unknown {
    return row[column.field];
  }

  protected isEnd(column: ColumnDef<T>): boolean {
    return (column.align ?? (column.type === 'number' || column.type === 'currency' ? 'end' : 'start')) === 'end';
  }

  protected currentSort(): SortState | null {
    return this.sort() ?? this.defaultSort();
  }

  protected ariaSort(column: ColumnDef<T>): 'ascending' | 'descending' | 'none' {
    const sort = this.currentSort();
    if (sort?.field !== column.field) return 'none';
    return sort.direction === 'asc' ? 'ascending' : 'descending';
  }

  /** First click sorts ascending, the next click flips the direction. */
  protected toggleSort(column: ColumnDef<T>): void {
    if (column.sortable === false) return;
    const current = this.currentSort();
    const direction = current?.field === column.field && current.direction === 'asc' ? 'desc' : 'asc';
    this.sort.set({ field: column.field, direction });
    this.pageIndex.set(0);
  }

  protected filterOf(column: ColumnDef<T>): ColumnFilter {
    return this.filters()[column.field] ?? { value: '', operator: 'eq' };
  }

  protected setFilterValue(column: ColumnDef<T>, value: string): void {
    this.patchFilter(column, { value });
  }

  protected setFilterOperator(column: ColumnDef<T>, operator: string): void {
    this.patchFilter(column, { operator: operator as ComparisonOperator });
  }

  protected clearFilters(): void {
    this.filters.set({});
    this.pageIndex.set(0);
  }

  protected setPageSize(size: string): void {
    this.pageSize.set(Number(size));
    this.pageIndex.set(0);
  }

  protected goToPage(index: number): void {
    this.pageIndex.set(Math.min(Math.max(0, index), this.pageCount() - 1));
  }

  protected onRowClick(row: T): void {
    if (this.rowClickSelects()) {
      this.select(row);
    }
    this.rowClick.emit(row);
  }

  protected select(row: T): void {
    this.selected.set(this.selected()?.id === row.id ? null : row);
  }

  protected onUpdateClick(): void {
    const row = this.selected();
    if (row) this.update.emit(row);
  }

  protected confirmDelete(): void {
    const row = this.pendingDelete();
    this.pendingDelete.set(null);
    if (row) this.deleteRow.emit(row);
  }

  private patchFilter(column: ColumnDef<T>, patch: Partial<ColumnFilter>): void {
    this.filters.update((filters) => ({ ...filters, [column.field]: { ...this.filterOf(column), ...patch } }));
    this.pageIndex.set(0);
  }
}
