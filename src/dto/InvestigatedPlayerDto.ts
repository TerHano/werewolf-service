/**
 * The subject of an investigation. The server deliberately does not send the player's
 * role here — an investigation only answers the question it was asked.
 */
export interface InvestigatedPlayerDto {
  id: number;
  nickname: string;
}
