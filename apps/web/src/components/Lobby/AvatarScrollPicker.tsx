import "swiper/swiper-bundle.css";

import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import { Avatar, Container, Float, HStack, IconButton } from "@chakra-ui/react";
import { Swiper, SwiperClass, SwiperSlide } from "swiper/react";
import styles from "./AvatarScrollPicker.module.css";
import { Navigation } from "swiper/modules";
import { useEffect, useRef } from "react";
import { IconArrowLeft, IconArrowRight } from "@tabler/icons-react";

const transitionSpeed = 100;

export const AvatarScrollPicker = ({
  initialAvatarIndex,
  setAvatarIndex,
}: {
  initialAvatarIndex: number;
  setAvatarIndex: (avatarIndex: number) => void;
}) => {
  const swipeRef = useRef<SwiperClass | null>(null);
  const { data: avatarNames, getAvatarImageSrcForIndex } = usePlayerAvatar();

  useEffect(() => {
    if (swipeRef.current && initialAvatarIndex !== undefined) {
      swipeRef.current.slideTo(initialAvatarIndex + 2);
    }
  }, [initialAvatarIndex]);

  return (
    <Container fluid p={0}>
      <Swiper
        loop
        modules={[Navigation]}
        spaceBetween={40}
        slidesPerView={3}
        centeredSlides
        onSlideChangeTransitionEnd={(swiper) => {
          setAvatarIndex(swiper.realIndex);
        }}
        onSwiper={(swipe) => {
          swipeRef.current = swipe;
        }}
      >
        {avatarNames.map((avatarName, index) => {
          return (
            <SwiperSlide key={avatarName} className={styles.swiper_slide}>
              {({ isActive }) => (
                <Avatar.Root
                  className={
                    isActive ? styles.active_slide : styles.inactive_slide
                  }
                  //   style={{
                  //     transition: "scale 200ms, opacity 200ms",
                  //     scale: isActive ? "1" : "0.6",
                  //     opacity: isActive ? "1" : ".4",
                  //   }}
                  variant="subtle"
                  size="full"
                  key={avatarName}
                >
                  <Avatar.Image
                    marginTop={1}
                    src={getAvatarImageSrcForIndex(index)}
                  />
                  <Avatar.Fallback>SA</Avatar.Fallback>
                </Avatar.Root>
              )}
            </SwiperSlide>
          );
        })}
        <Float placement="middle-center">
          <HStack gap="7rem">
            <IconButton
              onClick={() => {
                swipeRef.current?.slidePrev(transitionSpeed);
              }}
              variant="subtle"
              zIndex={1}
              size="xs"
            >
              <IconArrowLeft />
            </IconButton>
            <IconButton
              zIndex={1}
              size="xs"
              variant="subtle"
              onClick={() => {
                swipeRef.current?.slideNext(transitionSpeed);
              }}
            >
              <IconArrowRight />
            </IconButton>
          </HStack>
        </Float>
      </Swiper>
    </Container>
  );
};

export default AvatarScrollPicker;
