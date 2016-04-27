#!/bin/sh
dotnet restore
dotnet pack src/HtmlAgilityPack/project.json -o spider_nuget_packages 
dotnet pack src/HtmlAgilityPack.Css/project.json -o spider_nuget_packages
dotnet pack src/MySql.Data/project.json -o spider_nuget_packages
dotnet pack src/Newtonsoft.Json/project.json -o spider_nuget_packages
dotnet pack src/Java2Dotnet.Spider.Common/project.json -o spider_nuget_packages
dotnet pack src/Java2Dotnet.Spider.JLog/project.json -o spider_nuget_packages
dotnet pack src/Java2Dotnet.Spider.Redial/project.json -o spider_nuget_packages
dotnet pack src/Java2Dotnet.Spider.Validation/project.json -o spider_nuget_packages
dotnet pack src/Java2Dotnet.Spider.Core/project.json -o spider_nuget_packages
dotnet pack src/Java2Dotnet.Spider.Extension/project.json -o spider_nuget_packages
ftp -n<<!
open redis
user nuget 1qazZAQ!
binary
hash
lcd ~/solutions/DotnetSpider/spider_nuget_packages
prompt
mdelete /HtmlAgilityPack/0.0.9/*
rmdir /HtmlAgilityPack/0.0.9
rmdir /HtmlAgilityPack
mdelete /HtmlAgilityPack.Css/0.0.9/*
rmdir /HtmlAgilityPack.Css/0.0.9
rmdir /HtmlAgilityPack.Css
mdelete /MySql.Data/0.0.9/*
rmdir /MySql.Data/0.0.9
rmdir /MySql.Data
mdelete /Newtonsoft.Json/0.0.9/*
rmdir /Newtonsoft.Json/0.0.9
rmdir /Newtonsoft.Json
mdelete /Java2Dotnet.Spider.Common/0.0.9/*
rmdir /Java2Dotnet.Spider.Common/0.0.9
rmdir /Java2Dotnet.Spider.Common
mdelete /Java2Dotnet.Spider.JLog/0.0.9/*
rmdir /Java2Dotnet.Spider.JLog/0.0.9
rmdir /Java2Dotnet.Spider.JLog
mdelete /Java2Dotnet.Spider.Redial/0.0.9/*
rmdir /Java2Dotnet.Spider.Redial/0.0.9
rmdir /Java2Dotnet.Spider.Redial
mdelete /Java2Dotnet.Spider.Validation/0.0.9/*
rmdir /Java2Dotnet.Spider.Validation/0.0.9
rmdir /Java2Dotnet.Spider.Validation
mdelete /Java2Dotnet.Spider.Core/0.0.9/*
rmdir /Java2Dotnet.Spider.Core/0.0.9
rmdir /Java2Dotnet.Spider.Core
mdelete /Java2Dotnet.Spider.Extension/0.0.9/*
rmdir /Java2Dotnet.Spider.Extension/0.0.9
rmdir /Java2Dotnet.Spider.Extension
mput *
delete 86research02.cache.bin
close
bye
! 