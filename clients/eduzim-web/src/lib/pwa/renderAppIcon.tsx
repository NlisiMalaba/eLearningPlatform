import { ImageResponse } from "next/og";

type AppIconOptions = {
  size: number;
};

export function renderAppIcon({ size }: AppIconOptions): ImageResponse {
  const fontSize = Math.round(size * 0.38);

  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#0B6E4F",
          color: "#FFFFFF",
          fontSize,
          fontWeight: 700,
          letterSpacing: "-0.04em",
        }}
      >
        EZ
      </div>
    ),
    {
      width: size,
      height: size,
    },
  );
}
