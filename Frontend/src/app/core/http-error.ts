import { HttpErrorResponse } from '@angular/common/http';

/** Turns an HTTP error (usually an ASP.NET Core ProblemDetails) into a readable message. */
export function describeHttpError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Try again.';
  }

  if (error.status === 0) {
    return 'The server could not be reached. Check that the backend is running.';
  }

  const problem = error.error as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }

  return problem?.detail ?? problem?.title ?? `Request failed (${error.status}).`;
}

/** Validation messages for one field of an ASP.NET Core ValidationProblem (e.g. "Status"), if any. */
export function fieldErrors(error: unknown, field: string): string[] {
  if (!(error instanceof HttpErrorResponse)) return [];
  const errors = (error.error as { errors?: Record<string, string[]> } | null)?.errors;
  if (!errors) return [];
  const key = Object.keys(errors).find((k) => k.toLowerCase() === field.toLowerCase());
  return key ? errors[key] : [];
}
