interface Array<T> {
    remove(item: T): void;
    removeWhere(predicate: Function): void;
    fromObject(obj: any): Array<T>;
    //first(predicate: any, predicateOwner: any): any;
    first(predicate?: (item: T) => boolean): T | null;
    min(predicate: Function): any;
    max(predicate: Function): any;
    getUnique(): Array<T>;
    distinct(fieldName: any): Array<T>;
    where(predicate: (item: T, index: number) => boolean): Array<T>;
    addRange(arr: any): any;
    each(func: (item: T) => any): Array<T>;
    any(predicate: (item: T) => boolean): boolean;
    sum(predicate: any): number;
    checkDuplicateProperty(propertyList: any, item: any): any;
    lookupById(id: any, idProp: any): any;
    loadValuesById(objProp: any, idProp: any, list: any): void;
    group(groupSize: any): void;
    select<X>(predicate: (item: T) => X): X[];
    //indexOf2 (what: any, i: any) : any;
    getIndexById(id: any, idProp: any): number
}

//Array.prototype.indexOf2 = function (what, i) {
//    i = i || 0;
//    const l = this.length;
//    while (i < l) {
//        if (this[i] === what) return i;
//        ++i;
//    }
//    return -1;
//};;
Array.prototype.remove = function (item: any) {
    var index: any = this.indexOf(item);
    if (index === -1)
        return;
    if (index > 0) {
        this.splice(index, 1);
    }
    else if (index === 0) {
        this.shift();
    }
};

Array.prototype.removeWhere = function (predicate: any) {
    this.remove(this.first(predicate));
};
Array.prototype.fromObject = function (obj: any) {
    for (let p in obj) {
        if (obj.hasOwnProperty(p) === true) {
            this.push(obj[p]);
        }
    }
    return this;
};
Array.prototype.first = function (predicate?: (item: any) => boolean) {

    if (!predicate) {
        return this.length > 0 ? this[0] : null;
    }

    for (var i = 0, j = this.length; i < j; i++) {
        if (predicate(this[i])) {
            return this[i];
        }
    }

    return null;
};


Array.prototype.min = function (predicate: any) {
    var minValue: any = null;
    this.each((item: any) => {
        var value = predicate(item);
        if (minValue == null)
            minValue = value;
        else
            if (value < minValue)
                minValue = value;

    });
    return minValue;
}


Array.prototype.max = function (predicate: any) {
    var maxValue: any = null;
    this.each((item: any) => {
        var value = predicate(item);
        if (maxValue == null)
            maxValue = value;
        else
            if (value > maxValue)
                maxValue = value;

    });
    return maxValue;
}

Array.prototype.getUnique = function () {
    var u: any = {}, a: any[] = [];
    for (var i = 0, l = this.length; i < l; ++i) {
        if (u.hasOwnProperty(this[i])) {
            continue;
        }
        a.push(this[i]);
        u[this[i]] = 1;
    }
    return a;
};

Array.prototype.distinct = function (fieldName: any) {
    var u: any = {}, a: any[] = [];
    for (var i = 0, l = this.length; i < l; ++i) {
        if (u.hasOwnProperty(this[i][fieldName])) {
            continue;
        }
        a.push(this[i]);
        u[this[i][fieldName]] = 1;
    }
    return a;
};

Array.prototype.where = function (predicate: (item: any, index: number) => boolean) {
    var array = this;
    //array = array || [];
    var result: any[] = [];
    for (var i = 0, j = array.length; i < j; i++) {

        if (predicate(array[i], i)) {
            result.push(array[i]);
        }
    }
    return result;
};

Array.prototype.addRange = function (arr: any[]) {
    for (let i = 0, l = arr.length; i < l; i++) {
        this.push(arr[i]);
    }

    return this;
};
Array.prototype.each = function (func: (item: any) => any) {
    for (var i = 0, j = this.length; i < j; i++) {
        var breakValue = func(this[i]);
        if (breakValue === -1)
            return this;
    }
    return this;
};
Array.prototype.any = function (predicate: (item: any) => boolean) {

    for (var i = 0, j = this.length; i < j; i++) {
        if (predicate(this[i])) {
            return true;
        }
    }
    return false;
};

Array.prototype.sum = function (predicate: any) {
    if (this.length === 0)
        return 0;
    var value = 0;
    this.each((item: any) => {
        value += predicate(item);
    });
    return value;
}

Array.prototype.checkDuplicateProperty = function (propertyList: any, item: any) {
    var recursiveCheck = (oldItem: any, index: number): any => {
        if (index >= propertyList.length)
            return false;
        var prop = propertyList[index];
        if (oldItem[prop] === item[prop])
            return true;
        return recursiveCheck(oldItem, index + 1);
    };

    return this.first((oldItem: any) => {
        if (oldItem === item)
            return false;
        return recursiveCheck(oldItem, 0);
    });
}

Array.prototype.lookupById = function (id: any, idProp: any) {
    idProp = idProp || "Id";
    return this.first((item: any) => (item[idProp] === id));
}

Array.prototype.loadValuesById = function (objProp: any, idProp: any, list: any) {
    this.each((item: any) => {
        item[objProp] = list.LookupById(item[idProp]);
    });
}
Array.prototype.group = function (groupSize: any) {
    var results: any[] = [];
    var resultIndex = -1;
    var currentGroupSize = 0;
    this.each((item: any) => {
        if (currentGroupSize === 0) {
            results.push([]);
            resultIndex++;
        }
        results[resultIndex].push(item);
        currentGroupSize++;
        if (currentGroupSize > groupSize)
            currentGroupSize = 0;
    });
}


Array.prototype.select = function (predicate: any) {
    var retArr: any[] = [];
    for (var i = 0, j = this.length; i < j; i++) {
        retArr.push(predicate(this[i]));
    }
    return retArr;
};

Array.prototype.getIndexById = function (id: any, idProp: any): number {
    idProp = idProp || "Id";
    let index = -1;

    if (id > 0) {
        for (let i = 0; i < this.length; i++) {
            if (this[i][idProp] === id) {
                index = i;
                break;
            }
        }
    }

    return index;
};
