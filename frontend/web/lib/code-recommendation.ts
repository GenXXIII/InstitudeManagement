const recommendationPattern = /Recommended available code:\s*([A-Z0-9._/-]+)/i;

export function recommendedCodeFromError(message: string) {
  return message.match(recommendationPattern)?.[1]?.toUpperCase();
}

