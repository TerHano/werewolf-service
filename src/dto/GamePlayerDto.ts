/** A player in the game, keyed by player role id. Deliberately carries no role. */
export interface GamePlayerDto {
  id: number;
  nickname: string;
  avatarIndex: number;
  isAlive: boolean;
}
