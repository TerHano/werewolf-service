import { createLazyFileRoute } from "@tanstack/react-router";
import { MainMenu } from "../components/MainMenu/MainMenu";

export const Route = createLazyFileRoute("/")({
  component: Index,
});

function Index() {
  return <MainMenu />;
}
