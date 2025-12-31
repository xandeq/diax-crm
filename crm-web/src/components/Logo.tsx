import Image from 'next/image';

interface LogoProps {
  variant?: 'full' | 'icon';
  className?: string;
  width?: number;
  height?: number;
}

export function Logo({ variant = 'full', className = '', width, height }: LogoProps) {
  const src = variant === 'full'
    ? '/images/logo_xandao_400.png'
    : '/images/logo_logo.png';

  const alt = variant === 'full' ? 'Alexandre Queiroz CRM' : 'AQ Logo';

  // Default dimensions if not provided
  const defaultWidth = variant === 'full' ? 150 : 40;
  const defaultHeight = variant === 'full' ? 45 : 40;

  return (
    <div className={`relative flex items-center justify-center ${className}`}>
      <Image
        src={src}
        alt={alt}
        width={width || defaultWidth}
        height={height || defaultHeight}
        className="object-contain transition-opacity hover:opacity-90"
        priority
      />
    </div>
  );
}
