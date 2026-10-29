export interface ValidationProblem {
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}
