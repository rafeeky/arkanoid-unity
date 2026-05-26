// .NET Standard 2.1 에는 IsExternalInit 가 없어서 record/init 컴파일이 안 됨.
// 빈 타입 shim — C# 컴파일러가 init accessor 의 marker attribute 로 사용.
// Shared asmdef 가 모든 다른 asmdef 의 참조 대상이라 한 곳에만 두면 됨.
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}
