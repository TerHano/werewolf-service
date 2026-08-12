export interface RoomRoleSettingsDto {
  id: number;
  roomId: string;
  numberOfWerewolves: number;
  selectedRoles: number[];
  showGameSummary: boolean;
  allowMultipleSelfHeals: boolean;
  /** When true the server runs the night and everyone, host included, is dealt a role. */
  selfModerated: boolean;
  /** Length of every night step, including the ones nobody can act in. */
  nightStepSeconds: number;
}
