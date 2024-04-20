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