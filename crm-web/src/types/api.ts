export interface PromptErrorResponse {
  success: false;
  errorCode: string;
  message: string;
  provider?: string;
  correlationId?: string;
}

export interface PromptSuccessResponse {
  finalPrompt: string;
}

export type GeneratePromptResponse = PromptSuccessResponse | PromptErrorResponse;

export function isPromptError(response: any): response is PromptErrorResponse {
  return response && response.success === false && 'errorCode' in response;
}
