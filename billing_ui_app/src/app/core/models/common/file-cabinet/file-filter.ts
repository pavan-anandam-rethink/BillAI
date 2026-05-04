import * as moment from "moment";
import { MemberFile } from "./member-file";

export class FileFilter {
  keyword?: string;
  addedBy?: string[];
  createdDateRange?: FilterDateRange;
  tagIds?: number[]; 
  effectiveDateRange?: FilterDateRange;
  expirationDateRange?: FilterDateRange;

  public fileMeetsConditions(file: MemberFile): boolean {
    if (this.createdDateRange && !this.isDateInRange(file.dateUploaded, this.createdDateRange)) {
      return false;
    }

    if (this.effectiveDateRange && (!file.effectiveDate || !this.isDateInRange(file.effectiveDate, this.effectiveDateRange))) {
      return false;
    }

    if (this.expirationDateRange && (!file.expirationDate || !this.isDateInRange(file.expirationDate, this.expirationDateRange))) {
      return false;
    }

    if (this.tagIds && this.tagIds.length && !file.fileTags.some(t => !!this.tagIds && this.tagIds.indexOf(t.id) !== -1)) {
      return false;
    }

    if (this.addedBy && this.addedBy.length && !this.addedBy.some(userName => userName === file.createdBy)) {
      return false;
    }

    if (this.keyword) {
      const keywordInLowercase = this.keyword.toLowerCase();

      const keywordMatched = (!!file.folderName && file.folderName.toLowerCase().includes(keywordInLowercase)) || 
        file.fileName.toLowerCase().includes(keywordInLowercase) || 
        file.createdBy.toLowerCase().includes(keywordInLowercase) ||
        file.fileTags.some(t => t.name.toLowerCase().includes(keywordInLowercase));

      if (!keywordMatched) {
        return false;
      }
    }

    return true;
  }

  public hasAnyFilter(): boolean {
    return !!this.keyword || 
      !!this.createdDateRange || 
      !!this.effectiveDateRange ||
      !!this.expirationDateRange ||
      (!!this.addedBy && !!this.addedBy.length) || 
      (!!this.tagIds && !!this.tagIds.length);
  }

  private isDateInRange(date: Date, range: FilterDateRange) {
    const dateToCompare = moment(date);
    const start = moment(range.start);
    const end = moment(range.end);
    
    const inRange = !dateToCompare.isBefore(start, 'day') && !dateToCompare.isAfter(end, 'day');

    return inRange;
  }
}

export class FilterDateRange {
  start: Date;
  end: Date;
}