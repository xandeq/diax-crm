/**
 * Comprime uma imagem mantendo a qualidade visual aceitável.
 * Usa Canvas API nativa do browser - sem dependências.
 */

export interface ImageCompressionOptions {
  maxWidthOrHeight?: number;
  maxSizeKB?: number;
  quality?: number; // 0.0 to 1.0
}

export interface CompressedImage {
  dataUrl: string;
  originalSizeKB: number;
  compressedSizeKB: number;
  compressionRatio: number;
  width: number;
  height: number;
}

const DEFAULT_OPTIONS: Required<ImageCompressionOptions> = {
  maxWidthOrHeight: 1200,
  maxSizeKB: 300,
  quality: 0.85,
};

/**
 * Comprime uma imagem para otimizar tamanho do email.
 * @param file - Arquivo de imagem do input
 * @param options - Opções de compressão
 * @returns Imagem comprimida em base64
 */
export async function compressImage(
  file: File,
  options: ImageCompressionOptions = {}
): Promise<CompressedImage> {
  const opts = { ...DEFAULT_OPTIONS, ...options };

  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onerror = () => reject(new Error('Falha ao ler arquivo'));

    reader.onload = (e) => {
      const img = new Image();

      img.onerror = () => reject(new Error('Falha ao carregar imagem'));

      img.onload = () => {
        try {
          const originalSizeKB = Math.round(file.size / 1024);

          // Calcula novas dimensões mantendo aspect ratio
          let { width, height } = img;
          const maxDim = opts.maxWidthOrHeight;

          if (width > maxDim || height > maxDim) {
            if (width > height) {
              height = Math.round((height * maxDim) / width);
              width = maxDim;
            } else {
              width = Math.round((width * maxDim) / height);
              height = maxDim;
            }
          }

          // Cria canvas e desenha imagem redimensionada
          const canvas = document.createElement('canvas');
          canvas.width = width;
          canvas.height = height;

          const ctx = canvas.getContext('2d');
          if (!ctx) {
            reject(new Error('Falha ao criar contexto Canvas'));
            return;
          }

          // Melhora qualidade do redimensionamento
          ctx.imageSmoothingEnabled = true;
          ctx.imageSmoothingQuality = 'high';

          // Desenha imagem redimensionada
          ctx.drawImage(img, 0, 0, width, height);

          // Tenta comprimir até atingir tamanho alvo
          let quality = opts.quality;
          let dataUrl = '';
          let compressedSizeKB = 0;

          // Determina formato de saída
          const mimeType = file.type === 'image/png' ? 'image/png' : 'image/jpeg';

          // Loop de compressão iterativa
          for (let attempt = 0; attempt < 5; attempt++) {
            dataUrl = canvas.toDataURL(mimeType, quality);
            compressedSizeKB = Math.round((dataUrl.length * 3) / 4 / 1024); // estima tamanho base64

            if (compressedSizeKB <= opts.maxSizeKB || quality <= 0.5) {
              break; // Atingiu tamanho alvo ou qualidade mínima
            }

            // Reduz qualidade gradualmente
            quality -= 0.1;
          }

          resolve({
            dataUrl,
            originalSizeKB,
            compressedSizeKB,
            compressionRatio: originalSizeKB > 0 ? compressedSizeKB / originalSizeKB : 1,
            width,
            height,
          });
        } catch (error) {
          reject(error);
        }
      };

      img.src = e.target?.result as string;
    };

    reader.readAsDataURL(file);
  });
}

/**
 * Formata taxa de compressão para exibição.
 */
export function formatCompressionRatio(ratio: number): string {
  const percentage = Math.round((1 - ratio) * 100);
  return percentage > 0 ? `${percentage}% menor` : 'sem redução';
}
