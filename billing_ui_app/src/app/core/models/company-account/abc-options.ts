export class Antecedents {
  id: number;
  name: string;
}

export class Consequences {
  id: number;
  name: string;
}

export class Сontexts {
  id: number;
  name: string;
}

export class Reasons {
  id: number;
  name: string;
}

export class CommonAbcData {
  contexts: Сontexts[];
  antecedents: Antecedents[];
  consequences: Consequences[];
  reasons: Reasons[];
}

export class AbcData {
  abcData: CommonAbcData;
}