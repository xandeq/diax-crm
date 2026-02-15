export enum BlogPostStatus {
  Draft = 0,
  Published = 1,
  Archived = 2
}

export interface BlogPost {
  id: string;
  title: string;
  slug: string;
  contentHtml: string;
  excerpt: string;
  metaTitle?: string;
  metaDescription?: string;
  keywords?: string;
  status: string;
  statusDescription: string;
  publishedAt?: string;
  authorName: string;
  featuredImageUrl?: string;
  viewCount: number;
  isFeatured: boolean;
  category?: string;
  tagList?: string[];
  createdAt: string;
  updatedAt?: string;
}

export interface CreateBlogPostRequest {
  title: string;
  slug?: string;
  contentHtml: string;
  excerpt: string;
  metaTitle?: string;
  metaDescription?: string;
  keywords?: string;
  authorName: string;
  featuredImageUrl?: string;
  category?: string;
  tags?: string;
  publishImmediately?: boolean;
}

export interface UpdateBlogPostRequest {
  title: string;
  slug?: string;
  contentHtml: string;
  excerpt: string;
  metaTitle?: string;
  metaDescription?: string;
  keywords?: string;
  featuredImageUrl?: string;
  category?: string;
  tags?: string;
}

export interface BlogFilters {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: BlogPostStatus;
  category?: string;
}

export interface PagedBlogResponse {
  items: BlogPost[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
