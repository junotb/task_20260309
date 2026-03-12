namespace task_20260309.Application.Common;

/// <summary>
/// 키 기준 병합. 먼저 나온 항목 우선, 중복은 별도 수집.
/// Import 시 이메일/코드 등 기준 중복 제거에 사용.
/// </summary>
public static class MergeByKeyHelper
{
    /// <summary>
    /// keySelector로 키 추출, 중복 시 duplicates에 (Item, 1-based Index) 수집.
    /// keyComparer 미지정 시 OrdinalIgnoreCase.
    /// </summary>
    public static (List<T> Merged, List<(T Item, int Index)> Duplicates) MergeByKey<T>(
        IEnumerable<T> sources,
        Func<T, string> keySelector,
        IEqualityComparer<string>? keyComparer = null)
    {
        var merged = new List<T>();
        var duplicates = new List<(T Item, int Index)>();
        var seen = new HashSet<string>(keyComparer ?? StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var item in sources)
        {
            index++;
            var key = keySelector(item) ?? "";
            if (!seen.Add(key))
            {
                duplicates.Add((item, index));
                continue;
            }
            merged.Add(item);
        }
        return (merged, duplicates);
    }
}
