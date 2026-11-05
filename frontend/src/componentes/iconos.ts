import { BadgeDollarSign, Handshake, Landmark, Scale, Siren, UserX, type LucideIcon } from "lucide-react";
import type { CategoriaConducta } from "../api/tipos";

export const iconosCategoria: Record<CategoriaConducta, LucideIcon> = {
  Fraude: BadgeDollarSign,
  Soborno: Handshake,
  Corrupcion: Landmark,
  ConflictoIntereses: Scale,
  AcosoLaboral: UserX,
  Otra: Siren,
};
