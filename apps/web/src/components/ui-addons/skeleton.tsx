import type {
  SkeletonProps as ChakraSkeletonProps,
  CircleProps,
} from "@chakra-ui/react";
import {
  Box,
  Skeleton as ChakraSkeleton,
  Circle,
  Stack,
} from "@chakra-ui/react";
import { useDebounce } from "@uidotdev/usehooks";
import React from "react";

const loadingTransitionTimeMs = 250;

export interface SkeletonComposedProps {
  loading?: boolean;
  skeleton: React.ReactNode;
  children?: React.ReactNode;
}

export const SkeletonComposed = React.forwardRef<
  HTMLDivElement,
  SkeletonComposedProps
>(function SkeletonComposed({ loading, skeleton, children }, ref) {
  const _loading = useDebounce(loading, loadingTransitionTimeMs);
  return _loading ? (
    <Box
      ref={ref}
      w="inherit"
      height="inherit"
      data-state={loading ? "open" : "closed"}
      _closed={{
        animation: `fade-out ${loadingTransitionTimeMs}ms ease-out`,
      }}
    >
      {skeleton}
    </Box>
  ) : (
    <Box
      w="inherit"
      height="inherit"
      _open={{
        animation: `fade-in ${loadingTransitionTimeMs}ms ease-out`,
      }}
      data-state={loading ? "closed" : "open"}
    >
      {children}
    </Box>
  );
});

export interface SkeletonCircleProps extends ChakraSkeletonProps {
  size?: CircleProps["size"];
}

export const SkeletonCircle = React.forwardRef<
  HTMLDivElement,
  SkeletonCircleProps
>(function SkeletonCircle(props, ref) {
  const { size, children, loading, ...rest } = props;
  const _loading = useDebounce(loading, loadingTransitionTimeMs);
  return _loading ? (
    <Circle
      data-state={loading ? "open" : "closed"}
      _closed={{
        animation: `fade-out ${loadingTransitionTimeMs}ms ease-out`,
      }}
      size={size}
      asChild
      ref={ref}
    >
      <ChakraSkeleton loading {...rest} />
    </Circle>
  ) : (
    <Box
      _open={{
        animation: `fade-in ${loadingTransitionTimeMs}ms ease-out`,
      }}
      data-state={loading ? "closed" : "open"}
    >
      {children}
    </Box>
  );
});

export interface SkeletonTextProps extends ChakraSkeletonProps {
  noOfLines?: number;
}

export const SkeletonText = React.forwardRef<HTMLDivElement, SkeletonTextProps>(
  function SkeletonText(props, ref) {
    const { noOfLines = 3, gap, loading, children, ...rest } = props;
    const _loading = useDebounce(loading, loadingTransitionTimeMs);
    return _loading ? (
      <Stack
        w="inherit"
        height="inherit"
        data-state={loading ? "open" : "closed"}
        _closed={{
          animation: `fade-out ${loadingTransitionTimeMs}ms ease-out`,
        }}
        gap={gap}
        ref={ref}
      >
        {Array.from({ length: noOfLines }).map((_, index) => (
          <ChakraSkeleton
            loading
            height="4"
            key={index}
            {...props}
            _last={{ maxW: "80%" }}
            {...rest}
          />
        ))}
      </Stack>
    ) : (
      <Box
        w="inherit"
        height="inherit"
        _open={{
          animation: `fade-in ${loadingTransitionTimeMs}ms ease-out`,
        }}
        data-state={loading ? "closed" : "open"}
      >
        {children}
      </Box>
    );
  }
);

export const Skeleton = (props: ChakraSkeletonProps) => {
  const { loading, children, ...rest } = props;
  const _loading = useDebounce(loading, loadingTransitionTimeMs);

  if (_loading) {
    return (
      <Box
        w="inherit"
        height="inherit"
        data-state={loading ? "open" : "closed"}
        _closed={{
          animation: `fade-out ${loadingTransitionTimeMs}ms ease-out`,
        }}
      >
        <ChakraSkeleton {...rest} loading />
      </Box>
    );
  } else {
    return (
      <Box
        w="inherit"
        height="inherit"
        _open={{
          animation: `fade-in ${loadingTransitionTimeMs}ms ease-out`,
        }}
        data-state={loading ? "closed" : "open"}
      >
        {children}
      </Box>
    );
  }
};
