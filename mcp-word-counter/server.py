from fastmcp import FastMCP

mcp = FastMCP("Word Counter")


@mcp.tool()
def count_words(text: str) -> int:
    """Verilen metindeki kelime sayisini dondurur."""
    return len(text.split())


if __name__ == "__main__":
    mcp.run()
