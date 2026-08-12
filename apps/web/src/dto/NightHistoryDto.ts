import { GameActionDto } from "@/dto/GameActionDto";

export interface NightHistoryDto {
  night: number;
  nightActions: GameActionDto[];
  dayActions: GameActionDto[];
}
