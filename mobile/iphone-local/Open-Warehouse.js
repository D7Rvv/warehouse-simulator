// Scriptable本体で実行し、WarehouseStation-iPhone.htmlを選びます。
const files = args.fileURLs.length ? args.fileURLs : await DocumentPicker.open(["public.html"]);
if (files.length) {
  const view = new WebView();
  view.shouldAllowRequest = request => /^(file:|about:)/i.test(request.url);
  await view.loadFile(files[0]);
  await view.present(true);
}
Script.complete();
