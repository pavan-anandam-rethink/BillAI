/// <reference lib="webworker" />

self.onmessage = function (event) {
  const { url, token, model } = event.data;
  excelExport(url, model, token);
}

function excelExport(url: string, data: any, token: string) {
  fetch(url, {
      method: 'POST',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,

      },
      body: JSON.stringify(data)
    }).then(response => {
          if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
          }
          return response.json();
        })
        .then(result => {
          self.postMessage(result.data);
        })
        .catch(error => {
          postMessage({ error: error.message });
        });
}