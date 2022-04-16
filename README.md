<<<<<<< HEAD

<div align="center"><img src="./STDValidator.ico" /></div>

<div align="center">
    <h2 >STDValidator</h2>
</div>
=======
<img align="center" src="./STDValidator.ico">

<h2 align="center">STDValidator</h2>
>>>>>>> 53bb589c6b1ef581803fece51d386001cd6e5750

<center><b>我们不生产水，我们只是大自然的搬运工。</b></center>

![version](https://img.shields.io/badge/STDValidator-v1.1.1-orange)![build](https://img.shields.io/badge/build-passing-orange)![licence](https://img.shields.io/badge/Licence-MIT-orange)![GitHub Repo stars](https://img.shields.io/github/stars/3roman/stdvalidator)

本工具用于查询标准有效性，并返回最新替代标准号及标准名。数据来源为某专业网站，在此谢过！

## 信息输入

信息输入方式有以下三种：

1. 手动输入

   文本框内输入待查询标准号后点击**手动增加**按钮。

   标准号带年份即验证当前标准是否有效。不带年份就是查询当前有效的标准，比如`GB 12982`，软件能够自动将其刷新成当前有效的完整标准号。

2. 浏览目录

   浏览标准文件所在的目录，批量导入标准信息，要求文件命名格式同`GBT 13725-2019 建立术语数据库的一般原则与方法`。中间空格是半角或全角，空格有一个或多个，甚至没有都不打紧。

3. 批量导入

   新建 Excel 表格，第一列为标准号，第二列为标准名（可选），均无需列名，工作表名应为`Sheet1`。按行输入标准信息后点击**导入 Excel** 右键菜单批量导入。

   ## 查询结果

   点击**在线查询**按钮开始查询，后台采用`Task`并行机制，显著提高了查询效率。

   结果颜色说明：

   - 绿色—有效标准
   - 红色—过期标准
   - 蓝色—无结果

   点击**导出 Excel** 右键菜单可将当前所有结果导出。

   ## 其它说明

   多次查询后不再执行，其现象是颜色保持黑色，状态为**None**，这是因为达到了网站每天的查询限制数量。翌日再试！
