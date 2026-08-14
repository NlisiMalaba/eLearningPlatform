export function getClientApiBaseUrl(): string {
  return "";
}

export function getServerApiBaseUrl(): string {
  const configured = process.env.EDUZIM_API_URL?.trim();
  if (configured) {
    return configured.replace(/\/$/, "");
  }

  return "http://localhost:5196";
}
