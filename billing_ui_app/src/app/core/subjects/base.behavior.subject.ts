import { distinct } from '@progress/kendo-data-query';
import { BehaviorSubject } from 'rxjs';

interface IEntity {
    id: number;
}

export abstract class BaseBehaviorSubject<T extends IEntity> extends BehaviorSubject<T[]> {
    protected data: T[] = [];

    constructor() {
        super([]);
        this.data = [];
    }

    clear(): void {
        this.data = [];
        this.next(this.data);
    }

    getDistinctPrimitive(fieldName: string): T[] {
        return distinct(this.data, fieldName).map((item: any) => item[fieldName]);
    }

    sync(): void {
        this.next(this.data);
    }

    protected getIndex(item: T): number {
        let index = -1;

        if (item.id > 0) {
            index = this.data.getIndexById(item.id, 'id');
        } else {
            index = this.data.indexOf(item);
        }

        return index;
    };

    protected remove(item: T): void {
        const index = this.getIndex(item);

        if (index !== -1) {
            this.data.splice(index, 1);
        }
    }
}