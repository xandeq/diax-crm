export type PromptKind = 'prompt' | 'template' | 'system';
export type PromptSource = 'manual' | 'generator' | 'snippet' | 'import' | 'mcp' | 'git';
export type PromptVariableType = 'text' | 'textarea' | 'select' | 'number';

export interface PromptVariable {
  name: string;
  label?: string | null;
  type: PromptVariableType;
  default?: string | null;
  required: boolean;
  options?: string[] | null;
}

export interface PromptListItem {
  id: string;
  slug: string;
  title: string;
  description?: string | null;
  kind: PromptKind;
  framework?: string | null;
  collectionId?: string | null;
  tags: string[];
  preview: string;
  variableNames: string[];
  isFavorite: boolean;
  isArchived: boolean;
  exposeToMcp: boolean;
  currentVersionNumber: number;
  useCount: number;
  lastUsedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  source: PromptSource;
}

export interface PromptVersion {
  number: number;
  content: string;
  variables: PromptVariable[];
  changeNote?: string | null;
  source: PromptSource;
  frameworkApplied?: string | null;
  createdAt: string;
}

export interface PromptVersionSummary {
  number: number;
  changeNote?: string | null;
  source: PromptSource;
  frameworkApplied?: string | null;
  contentLength: number;
  createdAt: string;
}

export interface PromptDetail {
  prompt: PromptListItem;
  targetModel?: string | null;
  current: PromptVersion;
}

export interface PromptCollection {
  id: string;
  name: string;
  icon?: string | null;
  color?: string | null;
  sortOrder: number;
  promptCount: number;
}

export interface PromptTagCount {
  name: string;
  count: number;
}

export interface CreatePromptInput {
  title: string;
  content: string;
  description?: string | null;
  variables?: PromptVariable[];
  tags?: string[];
  collectionId?: string | null;
  kind?: PromptKind;
  framework?: string | null;
  targetModel?: string | null;
  exposeToMcp?: boolean;
  isFavorite?: boolean;
  changeNote?: string | null;
  source?: PromptSource;
}

export interface UpdatePromptInput {
  title?: string;
  slug?: string;
  description?: string;
  tags?: string[];
  collectionId?: string;
  clearCollection?: boolean;
  kind?: PromptKind;
  framework?: string;
  targetModel?: string;
  isFavorite?: boolean;
  isArchived?: boolean;
  exposeToMcp?: boolean;
}

export interface AddVersionInput {
  content: string;
  variables?: PromptVariable[];
  changeNote?: string | null;
  baseVersionNumber: number;
  source?: PromptSource;
  frameworkApplied?: string | null;
}
