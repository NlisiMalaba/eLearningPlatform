let inFlight: Promise<boolean> | null = null;

export async function refreshSessionOnce(
  refresh: () => Promise<unknown>,
): Promise<boolean> {
  if (!inFlight) {
    inFlight = refresh()
      .then(() => true)
      .catch(() => false)
      .finally(() => {
        inFlight = null;
      });
  }

  return inFlight;
}
