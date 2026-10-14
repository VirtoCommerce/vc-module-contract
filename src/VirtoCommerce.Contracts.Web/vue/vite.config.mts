import path from "node:path";
import { getDynamicModuleConfiguration } from "@vc-shell/mf-module";

// Builds the Contracts admin UI as a Module Federation remote of the Admin Shell host.
// Output: <module>/plugins/admin-shell/ (remoteEntry.js + public/plugin.json with the menu contribution),
// served by the platform at /modules/$(VirtoCommerce.Contracts)/plugins/admin-shell/.
const base = "/modules/$(VirtoCommerce.Contracts)/plugins/admin-shell/";
process.env.APP_BASE_PATH = base;

export default getDynamicModuleConfiguration({
  appId: "admin-shell",
  moduleRoot: path.resolve(__dirname, ".."),
  entry: "./src/modules/index.ts",
  remoteName: "contracts_admin",
  base,
});
