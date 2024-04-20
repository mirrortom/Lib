// --------------------------------------------
// cfg:公用配置对象. 方法小写开头,属性大写开头
// --------------------------------------------
let cfg = {};

// API 服务器地址,返回一个全路径地址.
cfg.apiUrl = (path) => { return 'http://localhost:20001' + path };
cfg.ApiLogin = cfg.apiUrl('/Srv/account/login');
cfg.ApiTokenCheck = cfg.apiUrl('/Srv/account/tokencheck');
cfg.ApiChgPwd = cfg.apiUrl('/Srv/account/chgpwd');
// 首页
cfg.ApiHomeIndex = cfg.apiUrl('/Srv/home/index');
// 人员
//
ns.cfg = cfg;