export interface TagFileModel {
  fileId: number;
  fileName: string;
}

export interface FileTagsToFiles {
  files: TagFileModel[];
  tagIds: number[];
}