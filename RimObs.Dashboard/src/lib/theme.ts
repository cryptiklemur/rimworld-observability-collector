export function cssVar(host: Element, name: string): string {
    return getComputedStyle(host).getPropertyValue(name).trim();
}

export function resolveColor(host: Element, color: string | undefined): string | undefined {
    if (color == null) return undefined;
    return color.startsWith('--') ? cssVar(host, color) : color;
}
