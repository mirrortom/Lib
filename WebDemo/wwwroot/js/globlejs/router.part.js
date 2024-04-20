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