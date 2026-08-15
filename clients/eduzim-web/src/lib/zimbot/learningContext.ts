const MODULE_PATH = /^\/modules\/([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/i;
const ASSESSMENT_PATH = /\/assessments?(\/|$)/i;

export function readModuleIdFromPath(pathname: string): string | undefined {
  const match = MODULE_PATH.exec(pathname);
  return match?.[1];
}

export function isAssessmentPath(pathname: string): boolean {
  return ASSESSMENT_PATH.test(pathname);
}

export function isLearningPath(pathname: string): boolean {
  return pathname === "/" || pathname.startsWith("/modules") || pathname.startsWith("/help");
}
