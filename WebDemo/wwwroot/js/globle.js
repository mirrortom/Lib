// ================================================
// 项目全局js库,包含接口约定.具体功能根据项目具体实现.
// ================================================
((win) => {
    // ----------------------
    // window对象上的命名空间.
    // ----------------------
    let ns = win.ns || {};

    // --------------------------------------------
    // 项目命名空间:页面脚本封闭函数中需要提供到外部使用的,
    // 对象,变量,函数,页面间传值等,统一绑定在此对象
    // --------------------------------------------
    ns.project = {};

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
// --------------------------------------------
// 预定错误提示语
// --------------------------------------------
let errtxt = {
    // 3位数约定为固定错误
    200: '服务器返回成功',
    500: '服务器发生异常',
    510: '拒绝请求',
    600: '操作失败',
    601: '没有数据',
    602: '参数错误',
    603: '数据库错误',
    // 4位数约定为自定义错误
    4001: '更新失败,源数据错误',
    4002: '未更新,内容没有修改',
    4003: '更新失败,无权操作'
};
// --
ns.errtxt = errtxt;
// -----------------
// 客户端token
// -----------------
let token = {};
let tokenkey = 'systoken';
// 存
token.newToken = (token) => {
    let tk = { token: token };
    localStorage.setItem(tokenkey, JSON.stringify(tk));
};
// 取
token.get = () => {
    let tkjson = localStorage.getItem(tokenkey);
    if (!tkjson) return null;
    let tk = JSON.parse(tkjson);
    return tk.token;
};
// 删除
token.del = () => {
    localStorage.removeItem(tokenkey);
    // 清除登录信息缓存
    //sessionStorage.removeItem('loginid');
};

ns.token = token;
// --------------------------------------------
// 项目相关工具函数
// --------------------------------------------
let tool = {};

// 公用带token的ajax
tool.get = (url, para, resType = 'html') => {
    let initCfg = { headers: { 'Auth': ns.token.get() } };
    return $.get(url, para, initCfg, resType);
};
tool.post = (url, para, resType = 'json') => {
    let initCfg = { headers: { 'Auth': ns.token.get() } };
    return $.post(url, para, initCfg, resType);
};
tool.getAsync = async (url, para, resType = 'html') => {
    let initCfg = { headers: { 'Auth': ns.token.get() } };
    return await $.getAsync(url, para, initCfg, resType);
};
tool.postAsync = async (url, para, resType = 'json') => {
    let initCfg = { headers: { 'Auth': ns.token.get() } };
    return await $.postAsync(url, para, initCfg, resType);
};
// 富文本里的图片显示前,加上路径.返回解码后的html
tool.convertEditorImgsToShow = (enHtml) => {
    let html = decodeURIComponent(enHtml);
    let box = $('<div>')[0];
    $(box).html(html).find('img').each((item) => {
        let imgSrc = $(item).prop('src');
        if (imgSrc.indexOf('/') == -1) {
            $(item).prop('src', '../img/articles/' + imgSrc);
        }
    });
    return $(box).html();
}
// 富文本里的图片保存前,去掉路径.返回编码后的html
tool.convertEditorImgsToSave = (html) => {
    let box = $('<div>')[0];
    let localImgDir = '../img/articles/';
    $(box).html(html).find('img').each((item) => {
        let imgSrc = $(item).prop('src');
        if (imgSrc.indexOf(localImgDir) == 0) {
            $(item).prop('src', imgSrc.replace(localImgDir, ''));
        }
    });
    //console.log($(box).html());
    return encodeURIComponent($(box).html());
}
//
ns.tool = tool;
// --------------------------------------------
// 主菜单路由
// --------------------------------------------
let router = {};
/**
 * 路由跳转url.一个函数,自定义实现.(限于router.urls对象里存在的页面地址)
 * @param {string} urlId (router.urls对象的id).
 * @param {any} para 页面传递参数.默认null
 */
router.goto = (urlId, para = null) => { };
/**
 * 跳转自定义页面,(自定义urlid,不在router.urls对象里的地址)
 * @param {any} urlId 唯一标识
 * @param {any} url 页面地址
 * @param {any} title 页面标题
 * @param {any} para 页面传递参数.默认null
 */
router.gotonew = (urlId, url,title, para = null) => { };

// 当前页面url对象的id.(router.urls对象的id).
// router.goto里会设置,tabs切换事件也会设置
router.urlId = null;

// 所有url,值是一个对象,包含url,title属性.也可以加其它属性 {urlkey:{url:,title:,page}}
// urlkey: 页面id,唯一
// url: 页面路径
// title: 页面标题
// page: 一个对象,记录这个页面的自定义数据,也可以放页面间传递的参数
router.urls = {
    "emplist": { url: "html/emplist.html", title: "列表", page: {} },
    "exit": { url: "html/exit.html", title: "退出", page: {} },
    "pwd": { url: "html/pwd.html", title: "修改密码", page: {} },
}
ns.router = router;
// 引用名称
if (!win.ns)
    win.ns = ns;
}) (window);