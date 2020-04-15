#利用asp.net core的运行时为基础构建镜像
From mcr.microsoft.com/dotnet/core/aspnet:2.2

#在linux根目录下的dockerWork文件夹
WORKDIR /dockerWorkApi

#将构建上下文所在目录的文件复制到工作目录（.就是构建上下文目录）
COPY .  /dockerWorkApi

#使用Docker的Run指令运行这个镜像时，会运行dotnet ReleaseInDocker.dll这个指令，后面可以加参数
#其实就是运行我们的项目
ENTRYPOINT ["dotnet","User.Api.dll"]