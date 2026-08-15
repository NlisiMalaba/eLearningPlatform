import { ModuleViewer } from "@/components/learning/ModuleViewer";

type ModulePageProps = {
  params: Promise<{ moduleId: string }>;
  searchParams: Promise<{ item?: string }>;
};

export default async function ModulePage({ params, searchParams }: ModulePageProps) {
  const { moduleId } = await params;
  const { item } = await searchParams;

  return <ModuleViewer moduleId={moduleId} initialContentItemId={item} />;
}
