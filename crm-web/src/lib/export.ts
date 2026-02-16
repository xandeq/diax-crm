/**
 * Export utilities for data tables
 */

export type ExportFormat = 'csv' | 'json';

/**
 * Converts data to CSV format
 */
export function convertToCSV<T extends Record<string, any>>(
  data: T[],
  columns?: string[]
): string {
  if (!data || data.length === 0) return '';

  // Use specified columns or all keys from first object
  const headers = columns || Object.keys(data[0]);

  // Create CSV header
  const csvHeader = headers.join(',');

  // Create CSV rows
  const csvRows = data.map((row) =>
    headers
      .map((header) => {
        const value = row[header];
        // Handle special characters and wrap in quotes if needed
        if (value === null || value === undefined) return '';
        const stringValue = String(value);
        // Escape quotes and wrap in quotes if contains comma or quote
        if (stringValue.includes(',') || stringValue.includes('"') || stringValue.includes('\n')) {
          return `"${stringValue.replace(/"/g, '""')}"`;
        }
        return stringValue;
      })
      .join(',')
  );

  return [csvHeader, ...csvRows].join('\n');
}

/**
 * Downloads data as a file
 */
export function downloadFile(content: string, filename: string, mimeType: string = 'text/plain') {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

/**
 * Exports data to CSV file
 */
export function exportToCSV<T extends Record<string, any>>(
  data: T[],
  filename: string,
  columns?: string[]
) {
  const csv = convertToCSV(data, columns);
  downloadFile(csv, `${filename}.csv`, 'text/csv;charset=utf-8;');
}

/**
 * Exports data to JSON file
 */
export function exportToJSON<T>(data: T[], filename: string) {
  const json = JSON.stringify(data, null, 2);
  downloadFile(json, `${filename}.json`, 'application/json');
}

/**
 * Formats date for export
 */
export function formatDateForExport(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toLocaleDateString('pt-BR');
}

/**
 * Sanitizes data for export (removes IDs, internal fields, etc.)
 */
export function sanitizeForExport<T extends Record<string, any>>(
  data: T[],
  fieldsToRemove: string[] = ['id']
): Partial<T>[] {
  return data.map((item) => {
    const cleaned: any = { ...item };
    fieldsToRemove.forEach((field) => delete cleaned[field]);
    return cleaned;
  });
}
