import { defineAppModule } from "@vc-shell/framework";
import * as blades from "./pages";
import * as locales from "./locales";

// The AngularJS module registers no widgets on other modules' blades and no dashboard widgets: blades only.
export default defineAppModule({
  blades,
  locales,
});
