/// <summary>
/// 版本号工具：纯数字三段 x.y.z 比较。
///
/// 为什么独立成一个工具类：
///   旧 ConfigUpdater.CompareVersion 里 int.TryParse 失败直接当 0，
///   导致 "1.2.3-beta" 这种版本号把 "beta" 当 0，比较结果不可预期。
///   这里统一规则：热更版本号强制纯数字三段，非纯数字由调用方用 TryParseStrict 判非法，
///   不进入比较（避免"容易报错"）。
/// </summary>
public static class VersionUtil
{
    /// <summary>比较 a、b：a>b 返回 1，相等 0，a<b 返回 -1。非法段按 0 处理。</summary>
    public static int Compare(string a, string b)
    {
        var pa = Parse(a);
        var pb = Parse(b);
        for (int i = 0; i < 3; i++)
        {
            if (pa[i] != pb[i]) return pa[i] > pb[i] ? 1 : -1;
        }
        return 0;
    }

    /// <summary>
    /// 严格解析：三段必须全为数字才返回 true。
    /// 用于"版本号格式校验"，不合法的版本号由调用方决定是报错还是视为需全量更新。
    /// </summary>
    public static bool TryParseStrict(string version, out int[] parts)
    {
        parts = new int[3];
        if (string.IsNullOrEmpty(version)) return false;
        var seg = version.Split('.');
        if (seg.Length != 3) return false;
        for (int i = 0; i < 3; i++)
            if (!int.TryParse(seg[i], out parts[i])) return false;
        return true;
    }

    /// <summary>宽松解析（内部用）：不足三段补 0，非法段按 0。</summary>
    private static int[] Parse(string version)
    {
        var result = new int[3];
        if (string.IsNullOrEmpty(version)) return result;
        var seg = version.Split('.');
        for (int i = 0; i < 3 && i < seg.Length; i++)
            result[i] = int.TryParse(seg[i], out var n) ? n : 0;
        return result;
    }
}
