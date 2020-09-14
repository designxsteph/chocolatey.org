module.exports = {
    header: {
        height: '2cm',
        contents: function (pageNum) {
            if (pageNum == 1) {
                return '<header class="pdf-header" style="padding-bottom: 20px;"><h2 style="text-align:center;margin:0;">Chocolatey Software Documentation</h2></header>'
            }
            return ''
        }
    },

    footer: {
        height: '1.5cm',
        contents: function (pageNum, numPages) {
            return '<footer class="pdf-footer" style="padding-top:20px;"><p style="float:left;width:33.33%;margin:0;font-size:10px;">' + new Date().toDateString() + '</p><p style="float:left;width:33.33%;margin:0;font-size:10px;text-align:center;">&copy; 2020 Chocolatey Software, Inc.</p><p style="float:right;width:33.33%;margin:0;font-size:10px;text-align:right;">Page ' + pageNum + ' of ' + numPages + '</p></footer>'
        }
    }
}